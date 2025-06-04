import * as d3 from "d3";
import { Link } from "react-router-dom";
import { useSize } from "ahooks";
import { useRef } from "react";
import invariant from "tiny-invariant";
import * as _ from "lodash";
import {
  createDefaultFilter,
  createInitialSourceTreeNode,
  createSubTreeNode,
  Layer,
  type AnyTreeNode,
} from "./model-live";
import { useModelContext } from "./Loader";
import { useIdParam } from "./common";
import { createBaseClass, deriveObject } from "./lib/utils";

export const ordinalColor = d3.scaleOrdinal<number, string>(
  d3.schemeCategory10
);

interface IEntryArgs {
  node: AnyTreeNode;
  layer: Layer;
  columnStart: number;
  columnEnd: number;
}

class Entry extends createBaseClass<IEntryArgs>() {
  getInfo() {
    return `${this.node.getDetailedInfo()} col ${this.columnStart}-${
      this.columnEnd
    }`;
  }
  getTitleInfo() {
    let t = this.getInfo();
    const p = this.node.parent;
    t += `\nparent: ${p ? p.getDetailedInfo() : "-"}`;
    return t;
  }
}

export function flattenWithColumns(treeNodes: AnyTreeNode[]) {
  const result: Entry[] = [];
  let currentColumn = 0;
  function descend(treeNodes: AnyTreeNode[]) {
    const n = treeNodes.length;
    for (let i = 0; i < n; ++i) {
      if (i > 0) {
        ++currentColumn;
      }
      const node = treeNodes[i];
      const entry = new Entry({
        node,
        layer: node.layer,
        columnStart: currentColumn,
        columnEnd: -1,
      });
      result.push(entry);
      descend(node.children);
      entry.columnEnd = currentColumn + 1;
    }
  }
  descend(treeNodes);
  return result;
}

function groupByLayer(entries: Entry[], columnsExtent: [number, number]) {
  const [columnsMin, columnsMax] = columnsExtent;
  const columnsLength = columnsMax - columnsMin;

  const map = d3.group(entries, (e) => e.layer.number);
  const groups = [...map].map(([layerNumber, entriesInGroup]) => {
    return {
      rowType: "taxons" as RowType,
      layerNumber,
      rank: entriesInGroup[0].node.layer.rank,
      entries: entriesInGroup.map((e) =>
        deriveObject(e, {
          left: `${((e.columnStart - columnsMin) / columnsLength) * 100}%`,
          width: `${((e.columnEnd - e.columnStart) / columnsLength) * 100}%`,
        })
      ),
    };
  });
  groups.sort((l, r) => l.layerNumber - r.layerNumber);
  return groups;
}

const rowSizes = {
  special: [100, 20],
  other: [25, 0],
  breadcrumb: 25,
};

function getRowElements(
  rootNode: AnyTreeNode,
  entries: Entry[],
  columnsExtent: [number, number]
) {
  return [
    ...getBreadcrumbRowElements(rootNode),
    ...getTaxonRowElements(entries, columnsExtent),
  ];
}

function getBreadcrumbRowElements(rootNode: AnyTreeNode) {
  return rootNode.ancestors.reverse().map((node) => {
    const size = rowSizes.breadcrumb;
    const layer = node.layer;
    const layerNumber = layer.number;
    return {
      rowType: "breadcrumb" as RowType,
      layer,
      layerNumber,
      node,
      size,
    };
  });
}

function getTaxonRowElements(
  entries: Entry[],
  columnsExtent: [number, number]
) {
  const groups = groupByLayer(entries, columnsExtent);

  return _.chain(groups)
    .map((g) => {
      const [innerSize, margin] = g.rank?.isSpecial
        ? rowSizes.special
        : rowSizes.other;
      return {
        ...g,
        innerSize,
        margin,
        size: innerSize + 2 * margin,
      };
    })
    .value();
}

type RowType = "taxons" | "breadcrumb";

type TaxonsRow = ReturnType<typeof getTaxonRowElements>[number];
type BreadcrumbRow = ReturnType<typeof getBreadcrumbRowElements>[number];
type Row = TaxonsRow | BreadcrumbRow;

export function StructureView() {
  const model = useModelContext();

  const rootI = useIdParam();

  const sourceTreeNode = createInitialSourceTreeNode(model, rootI);

  const rootRankLevel = sourceTreeNode.raw.rank_level;

  const filter = createDefaultFilter(model, (rootRankLevel / 10 + 2) * 10);

  const subTreeNode = createSubTreeNode(sourceTreeNode, filter);

  const data = flattenWithColumns([subTreeNode]);

  const columnsExtent: [number, number] = [
    d3.min(data.map((t) => t.columnStart))!,
    d3.max(data.map((t) => t.columnEnd))!,
  ];

  invariant(typeof columnsExtent[0] === "number");
  invariant(typeof columnsExtent[1] === "number");

  const wrapperRef = useRef<HTMLDivElement>(null);
  const size = useSize(wrapperRef);

  const rowElements = getRowElements(subTreeNode, data, columnsExtent);

  const rowOffsets = d3.cumsum(rowElements.map((r) => r.size));

  const rootEntry = data[0];

  console.info(
    `rendering ${data.length} items, root is ${rootEntry.node.sourceTreeNode.name} ${rootEntry.node.layer}`
  );

  return (
    <div className="h-screen flex flex-col">
      <Details node={subTreeNode} />
      <div className="flex-1" />
      <div
        ref={wrapperRef}
        className="relative grid w-full overflow-hidden"
        style={{ height: rowOffsets[rowOffsets.length - 1] }}
      >
        {rowElements.map((r, ri) => (
          <Row key={r.layerNumber} row={r} offset={rowOffsets[ri - 1]} />
        ))}
      </div>
    </div>
  );
}

function Row(props: { row: Row; offset: number }) {
  const rowType = props.row.rowType;
  const { row, offset } = props;
  function getContent() {
    if (rowType === "taxons") {
      return <TaxonsRow row={row as TaxonsRow} />;
    } else if (rowType === "breadcrumb") {
      return <BreadcrumbRow row={row as BreadcrumbRow} />;
    } else {
      throw Error(`Unknown row type ${rowType}`);
    }
  }
  return (
    <div
      key={row.layerNumber}
      className="absolute w-full"
      style={{
        top: offset ?? 0,
        height: row.size,
        //backgroundColor: color,
      }}
    >
      {getContent()}
    </div>
  );
}

function BreadcrumbRow(props: { row: BreadcrumbRow }) {
  const { row } = props;
  return (
    <div>
      <Link
        className="absolute truncate w-full"
        key={row.layerNumber}
        to={{
          pathname: "/",
          search: `?id=${row.node.item_number}`,
        }}
        style={{
          backgroundColor: ordinalColor(row.node.item_number),
          height: row.size,
        }}
      >
        {`${row.layer} ${row.node.name}`}
      </Link>
    </div>
  );
}

function TaxonsRow(props: { row: TaxonsRow }) {
  const { row } = props;
  return (
    <>
      {row.rank?.isSpecial && (
        <label
          className="leading-none font-bold text-[18px]"
          style={{
            paintOrder: "stroke",
            WebkitTextStroke: "2px white",
          }}
        >
          {row.rank?.name}
        </label>
      )}

      {row.entries.map((e) => {
        return (
          <Link
            className="absolute truncate"
            key={e.node.item_number}
            title={e.getTitleInfo()}
            to={{
              pathname: "/",
              search: `?id=${e.node.item_number}`,
            }}
            style={{
              backgroundColor: ordinalColor(e.node.item_number),
              left: e.left,
              top: row.margin,
              width: e.width,
              height: row.innerSize,
            }}
          >
            {e.getInfo()}
          </Link>
        );
      })}
    </>
  );
}

function Details({ node }: { node: AnyTreeNode }) {
  return (
    <div className="p-4">
      <div className="text-2xl">{node.name}</div>
      <pre>{JSON.stringify(node.raw, null, 2)}</pre>
      <pre>{JSON.stringify(node.rank, null, 2)}</pre>
    </div>
  );
}
