import { useModelContext } from "./Loader";
import {
  createInitialSourceTreeNode,
  createPuddleFilter,
  createSubTreeNode,
  type AnyTreeNode,
} from "./model-live";
import * as _ from "lodash";
import * as Inputs from "@observablehq/inputs";
import { useEffect, useRef, type ReactNode } from "react";
import { createRoot } from "react-dom/client";
import "@observablehq/inputs/dist/index.css";
import { html } from "htl";
import { intersperse } from "./lib/utils";

function renderReactNode(children: ReactNode) {
  const element = document.createElement("div");
  createRoot(element).render(children);
  return element;
}

function ForeignElement(props: { get: () => HTMLElement }) {
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const c = ref.current!;
    while (c.children.length) {
      c.removeChild(c.firstChild!);
    }
    c.appendChild(props.get());
  }, []);

  return <div ref={ref} />;
}

export function Defects({}) {
  const model = useModelContext();
  const { rankPuddleRoots } = model;

  function createPuddleSubTreeNode(i: number) {
    const filter = createPuddleFilter();
    const sourceTreeNode = createInitialSourceTreeNode(model, i);
    const subTreeNode = createSubTreeNode(sourceTreeNode, filter);
    return subTreeNode;
  }

  const rankPuddleRootNodes = [...rankPuddleRoots].map(createPuddleSubTreeNode);

  const groups = _.chain(rankPuddleRootNodes)
    .groupBy((n) => n.raw.rank_level)
    .orderBy((g) => g[0].raw.rank_level)
    .value();
  return (
    <>
      <div className="container max-w-xl mx-auto flex flex-col gap-4">
        <div className="font-bold">Number of puddles by rank</div>
        <ForeignElement
          get={() =>
            Inputs.table(
              groups.map((g) => {
                const n0 = g[0];
                const rank = n0.rank;
                const rank_level = n0.raw.rank_level;
                return {
                  rank: rank ?? "unranked",
                  rank_level,
                  size: g.length,
                  // items: g,
                };
              })
              // {
              //   format: {
              //     items: (g: AnyTreeNode[]) =>
              //       renderReactNode(
              //         <div className="text-xs w-2xl text-wrap">
              //           {intersperse<ReactNode>(
              //             ", ",
              //             g.map((n) => (
              //               <span key={n.item_number} className="">
              //                 {n.name}
              //               </span>
              //             ))
              //           )}
              //         </div>
              //       ),
              //   },
              // }
            )
          }
        />
        <div className="font-bold">All puddles</div>
        <ForeignElement
          get={() =>
            Inputs.table(
              rankPuddleRootNodes.map((n) => ({
                name: n.name,
                rank: n.rank ?? "unranked",
                rankLevel: n.raw.rank_level,
              }))
            )
          }
        />
      </div>
    </>
  );
}
