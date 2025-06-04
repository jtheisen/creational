import invariant from "tiny-invariant";
import type { Model, Rank, Rankings, Raw } from "./model";

const enableTreeNodeChildCollectingLogging = false;

export class Layer {
  rank: Rank | undefined;
  declaredRank: Rank | undefined;
  rank_level: number;
  rank_sub_level: number;
  flag: string;

  constructor(
    rank: Rank | undefined,
    rank_level: number,
    rank_sub_level: number
  ) {
    invariant(rank_level >= 0);
    invariant(!rank || rank.level == rank_level);
    this.flag = rank ? (rank_sub_level ? "i" : "t") : "u";
    this.declaredRank = rank;
    this.rank = this.flag === "t" ? rank : undefined;
    this.rank_level = rank_level;
    this.rank_sub_level = rank_sub_level;
  }

  get number() {
    return this.rank_level * 256 + this.rank_sub_level;
  }

  toString() {
    return `Lx${this.number.toString(16).padStart(4, "0")}${this.flag}`;
  }
}

abstract class TreeNode<D extends TreeNode<D>> {
  abstract item_number: number;

  abstract raw: Raw;

  abstract nestingDepth: number;

  abstract sourceTreeNode: SourceTreeNode;

  abstract parent: D | null;

  abstract model: Model;

  markedForeignNode?: SubTreeNode;

  matchingForeignFilterRule?: FilterRule;

  // Children

  private cachedChildren: D[] | null = null;

  protected collectChildren(_: (chid: D) => any) {}

  get children() {
    if (this.cachedChildren === null) {
      if (enableTreeNodeChildCollectingLogging)
        console.info(`collecting children for ${this}`);
      this.cachedChildren = [];
      this.collectChildren((c) => this.cachedChildren?.push(c));
      for (let child of this.cachedChildren) {
        if ((child as any) === this) {
          throw Error(`Node is its own child`);
        }
      }
      if (enableTreeNodeChildCollectingLogging)
        console.info(
          `collected children for ${this}: ${this.cachedChildren.join(",")}`
        );
    }
    return this.cachedChildren;
  }

  // Layer

  private cachedLayer?: Layer;

  get layer(): Layer {
    if (!this.cachedLayer) {
      this.cachedLayer = this.calculateLayer();
    }
    return this.cachedLayer;
  }

  private calculateLayer(): Layer {
    if (
      this.raw.rank_level >= 0 &&
      this.raw.rank_smear_top_item_number === this.item_number
    ) {
      // We're ranked and the first item in the path with this rank
      const rank = this.model.rankings.getRankObjectOrNull(this.raw.rank_level);
      invariant(rank);
      return new Layer(rank, rank.level, 0);
    } else if (this.parent) {
      // Otherwise we're one sub-level below our parent's rank
      const parentLayer = this.parent.layer;
      return new Layer(
        parentLayer.declaredRank,
        parentLayer.rank_level,
        parentLayer.rank_sub_level + 1
      );
    } else {
      return new Layer(undefined, 0, 0);
    }
  }

  // Ancestors

  get ancestors() {
    const result: D[] = [];
    let current: D | null = this as any as D;
    while ((current = current.parent)) {
      result.push(current);
    }
    return result;
  }

  // Descendants

  get descendants() {
    const result: TreeNode<D>[] = [];
    this.forSelfAndDescendants((n) => result.push(n));
    return result;
  }

  static globalTimerId = 0;

  materialize() {
    const timer = `Materializing tree at nesting depth ${
      this.nestingChar
    } (${++TreeNode.globalTimerId})`;
    console.time(timer);
    this.forSelfAndDescendants();
    console.timeEnd(timer);
  }

  forSelfAndDescendants(action: ((node: D) => any) | undefined = undefined) {
    if (action) {
      action(this as any as D);
    }
    for (let child of this.children) {
      child.forSelfAndDescendants(action);
    }
  }

  clearMarks() {
    this.forSelfAndDescendants((n) => (n.markedForeignNode = undefined));
  }

  clearMatchingForeignFilterRule() {
    this.forSelfAndDescendants(
      (n) => (n.matchingForeignFilterRule = undefined)
    );
  }

  markForeignNodes(subTreeRoot: SubTreeNode) {
    this.clearMarks();
    subTreeRoot.forSelfAndDescendants(
      (n) => (n.fullTreeNode.markedForeignNode = n)
    );
  }

  get nestingChar() {
    return String.fromCharCode("A".charCodeAt(0) + this.nestingDepth);
  }

  // Formatting

  toString() {
    return `#${this.nestingChar}${toSubscript(`${this.item_number}`)}`;
  }

  getDetailedRankInfo() {
    const ch = this.layer.rank?.isSpecial ? "SR" : "R";
    return `${this.model.rankings.rankById[this.raw.rank_level] ?? ""} ${ch}${
      this.raw.rank_level
    } ${this.layer}`.trim();
  }

  getDetailedInfo() {
    return `${this.model.names[this.item_number]} (#${
      this.item_number
    } ${this.getDetailedRankInfo()})`;
  }
}

function toSubscript(n: string) {
  return String(n)
    .split("")
    .map((c) => {
      const code = c.charCodeAt(0);
      return c >= "0" && c <= "9"
        ? String.fromCharCode(0x2080 + (code - 48))
        : c;
    })
    .join("");
}

class SourceTreeNode extends TreeNode<SourceTreeNode> {
  constructor(repo: Repo, i: number) {
    super();
    invariant(typeof i === "number" && !Number.isNaN(i));
    this.item_number = i;
    this.repo = repo;
    this.nestingDepth = 0;
    const sourceTree = repo.model.sourceTree;
    this.raw = sourceTree.object(i) as any as Raw;
  }

  readonly item_number: number;
  readonly raw: Raw;
  readonly repo: Repo;
  readonly nestingDepth: number;

  get sourceTreeNode() {
    return this;
  }

  get model() {
    return this.repo.model;
  }

  get name() {
    return this.repo.model.names[this.item_number];
  }

  get parent(): SourceTreeNode | null {
    const { parent_item_number } = this.raw;
    if (parent_item_number < 0) {
      return null;
    }
    return this.repo.get(parent_item_number);
  }

  get rank() {
    return this.repo.model.rankings.getRankObjectOrNull(this.raw.rank_level);
  }

  get isSpecial() {
    return this.repo.model.rankings.isRankSpecialById[this.raw.rank_level];
  }

  collectChildren(add: (chid: SourceTreeNode) => any) {
    const cn = this.raw.child_count;
    let current_item_number = this.item_number + 1;
    for (let i = 0; i < cn; ++i) {
      const child = this.repo.get(current_item_number);
      add(child);
      current_item_number += child.raw.descendant_count + 1;
    }
  }
}

export function createSourceTreeRepo(model: Model) {
  const cache: SourceTreeNode[] = [];
  return {
    model,
    get(item_number: number) {
      const existing = cache[item_number];
      return (
        existing ?? (cache[item_number] = new SourceTreeNode(this, item_number))
      );
    },
  };
}

type Repo = ReturnType<typeof createSourceTreeRepo>;

export function createInitialSourceTreeNode(model: Model, i: number) {
  const repo = createSourceTreeRepo(model);
  return new SourceTreeNode(repo, i);
}

class SubTreeNode extends TreeNode<SubTreeNode> {
  constructor(
    filter: Filter,
    fullTreeNode: AnyTreeNode,
    cachedParentSubTreeNode?: SubTreeNode
  ) {
    super();
    this.item_number = fullTreeNode.item_number;
    this.filter = filter;
    this.fullTreeNode = fullTreeNode;
    this.nestingDepth = fullTreeNode.nestingDepth + 1;
    this.sourceTreeNode = fullTreeNode.sourceTreeNode;
    this.cachedParent = cachedParentSubTreeNode;
    this.filterMatchingRule = filter.evaluate(fullTreeNode);
    invariant(typeof this.item_number !== "undefined");
    const filterResult = this.filterMatchingRule.result;
    this.isLeaf = this.filterMatchingRule.result === "self";
    if (!Filter.FilterResults.includes(filterResult)) {
      throw Error(`Filter result was unexpectedly ${filterResult}`);
    }
  }

  columnStart = null;
  columnEnd = null;

  item_number: number;
  filter: Filter;
  fullTreeNode: AnyTreeNode;
  nestingDepth: number;
  sourceTreeNode: SourceTreeNode;

  filterMatchingRule: FilterRule;
  isLeaf: boolean;

  get model() {
    return this.sourceTreeNode.model;
  }

  get name() {
    return this.sourceTreeNode.name;
  }

  get filterResult() {
    return this.filterMatchingRule.result;
  }

  get raw() {
    return this.sourceTreeNode.raw;
  }

  get rank() {
    return this.sourceTreeNode.rank;
  }

  get isSpecial() {
    return this.sourceTreeNode.isSpecial;
  }

  // Parents

  cachedParent?: SubTreeNode | null;

  get parent() {
    if (this.cachedParent === undefined) {
      this.cachedParent = this.calculateParent();
    }
    return this.cachedParent;
  }

  private static shouldIncludeNodeWithFilterResult(
    filterMatchingRule: FilterRule
  ) {
    const { result } = filterMatchingRule;
    switch (result) {
      case "all":
        return true;
      case "children":
      case "none":
        return false;
      case "self":
        throw Error(
          `Rule ${filterMatchingRule.name} with result ${result} should not apply to an ancestor node`
        );
    }
  }

  private calculateParent(): SubTreeNode | null {
    let fullTreeNode: AnyTreeNode | null = this.fullTreeNode;
    while ((fullTreeNode = fullTreeNode.parent)) {
      const potentialSubTreeNode = new SubTreeNode(this.filter, fullTreeNode);
      const { filterMatchingRule } = potentialSubTreeNode;
      if (SubTreeNode.shouldIncludeNodeWithFilterResult(filterMatchingRule)) {
        return potentialSubTreeNode;
      }
    }
    return null;
  }

  // Children

  protected collectChildren(add: (chid: SubTreeNode) => any) {
    if (this.isLeaf) return;
    collectChildren(add, this.fullTreeNode, this.filter, this);
  }
}

export function createSubTreeNode(fullTreeNode: AnyTreeNode, filter: Filter) {
  return new SubTreeNode(filter, fullTreeNode);
}

function collectChildren(
  add: (chid: SubTreeNode) => any,
  fullTreeNode: AnyTreeNode,
  filter: Filter,
  parent: SubTreeNode
) {
  for (let fullTreeChild of fullTreeNode.children) {
    const childSubTreeNode = new SubTreeNode(filter, fullTreeChild, parent);
    const matchingFilterRule = childSubTreeNode.filterMatchingRule;
    const filterResult = matchingFilterRule.result;
    fullTreeChild.matchingForeignFilterRule = matchingFilterRule;
    switch (filterResult) {
      case "self":
      case "all":
        //console.info(`Including ${childSubTreeNode} as per ${ruleName}`);
        add(childSubTreeNode);
        break;
      case "children":
        // console.info(
        //   `Excluding self of ${childSubTreeNode} as per ${ruleName}`
        // );
        collectChildren(add, fullTreeChild, filter, parent);
        break;
      case "none":
        // console.info(`Excluding all of ${childSubTreeNode} as per ${ruleName}`);
        break;
      default:
        throw Error(`Unexpected filter result ${filterResult}`);
    }
  }
}

export type AnyTreeNode = SourceTreeNode | SubTreeNode;

const FilterResults = ["all", "self", "children", "none"] as const;

type FilterResult = (typeof FilterResults)[number];

class FilterRule {
  constructor(
    name: string,
    result: FilterResult,
    predicate: (n: AnyTreeNode) => boolean,
    diagnostic?: (n: AnyTreeNode) => string
  ) {
    this.name = name;
    this.result = result;
    this.predicate = predicate;
    this.diagnostic = diagnostic;
  }

  name: string;
  result: FilterResult;
  predicate: (n: AnyTreeNode) => boolean;
  diagnostic?: (n: AnyTreeNode) => string;

  toString() {
    return this.toStringWithEvaluation();
  }

  toStringWithEvaluation(node?: AnyTreeNode) {
    const getEvaluationString = this.diagnostic;
    const suffix =
      node && getEvaluationString ? `; ${getEvaluationString(node)}` : "";
    return `[${this.name}: ${this.result}${suffix}]`;
  }
}

export function buildFilter(
  buildRules: (
    add: (
      name: string,
      result: FilterResult,
      predicate: (n: AnyTreeNode) => boolean,
      diagnostic?: (n: AnyTreeNode) => string
    ) => any
  ) => any
) {
  const rules: FilterRule[] = [];

  buildRules(
    (
      name: string,
      result: FilterResult,
      predicate: (n: AnyTreeNode) => boolean,
      diagnostic?: (n: AnyTreeNode) => string
    ) => rules.push(new FilterRule(name, result, predicate, diagnostic))
  );

  return new Filter(rules);
}

export class Filter {
  constructor(rules: FilterRule[]) {
    this.rules = rules;
  }

  rules: FilterRule[];

  knownItemNumbers = new Set();

  static FilterResults = ["all", "self", "children", "none"];

  static DefaultFinalRule: FilterRule = new FilterRule(
    "default",
    "all",
    () => true
  );

  onlyFirstN(n: number) {
    return new Filter([...this.rules.slice(0, n), Filter.DefaultFinalRule]);
  }

  evaluate(node: AnyTreeNode) {
    if (this.knownItemNumbers.has(node.item_number)) {
      throw Error(`Item ${node} already evaluated`);
    }
    this.knownItemNumbers.add(node.item_number);
    for (let rule of this.rules) {
      const { predicate } = rule;
      const isMatch = predicate(node);
      if (isMatch) {
        return rule;
      }
    }
    throw Error(`No filter rule matched`);
  }
}

export function createPuddleFilter() {
  return buildFilter((add) => {
    add(
      "different rank",
      "none",
      (t) => t.parent?.raw.rank_level !== t.raw.rank_level
    );
    add("default", "all", (t) => true);
  });
}

function loggingTag(strings: TemplateStringsArray, obj: any) {
  const message = strings.join(" * ");
  console.info(message, obj);
  return message;
}

export function createDefaultFilter(model: Model, maxLevel = 5) {
  const { rankings } = model;
  const { specialRanks } = rankings;
  return buildFilter((add) => {
    add("root taxon", "all", (t) => t.item_number === 0);
    add(
      "maxlevel",
      "none",
      (t) => {
        invariant(
          typeof t.raw.rank_level === "number",
          () => loggingTag`error: ${{ t }}`
        );
        return t.raw.rank_level > maxLevel;
      },
      (t) => `${t.raw.rank_level} > ${maxLevel}`
    );
    add("unranked taxon", "children", (t) => t.raw.rank_level < 0);
    add(
      "domains & kingdoms",
      "all",
      (t) => t.rank?.name === "domain" || t.rank?.name == "kingdom"
    );
    add(
      "non-special ranks",
      "children",
      (t) => {
        return t.rank ? !specialRanks.includes(t.rank.name) : false;
      },
      (t) => t.rank?.name ?? ""
    );
    add("default", "all", (_) => true);
  });
}

export function flatten(treeNodes: AnyTreeNode[]) {
  const result: AnyTreeNode[] = [];
  function descend(target: AnyTreeNode[], treeNodes: AnyTreeNode[]) {
    for (let node of treeNodes) {
      target.push(node);
      descend(target, node.children);
    }
  }
  descend(result, treeNodes);
  return result;
}
