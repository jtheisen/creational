import { useSearchParams } from "react-router-dom";

export function useIdParam() {
  const [searchParams] = useSearchParams();
  const idString = searchParams.get("id");

  try {
    return parseInt(idString ?? "0");
  } catch (ex) {
    console.error(`Expected number as taxon id, using zero instead`);
    return 0;
  }
}
