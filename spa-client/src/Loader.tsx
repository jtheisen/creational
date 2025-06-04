import { useRequest } from "ahooks";
import { fetchModelCached } from "./model-loading";
import { createContext, useContext, type ReactNode } from "react";
import type { Model } from "./model";

/**
 * This indirection hack is necessary to prevent vite from reloading everything,
 * an in particular from making it re-recreate the tree.
 */
const loadingPromise = fetchModelCached();

async function fetchModelLocal() {
  const model = await loadingPromise;
  return model;
}

const ModelContext = createContext<Model>(undefined!);

export function useModelContext() {
  return useContext(ModelContext);
}

export default function Loader({ children }: { children: ReactNode }) {
  const { data, error, loading } = useRequest(fetchModelLocal, {
    refreshDeps: [],
  });

  if (loading) {
    return <div>loading</div>;
  } else if (error) {
    return <div>error while loading</div>;
  } else if (!data) {
    return "initializing";
  } else {
    return <ModelContext value={data}>{children}</ModelContext>;
  }
}
