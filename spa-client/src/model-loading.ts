import { fetchModel, type Model } from "./model";

let model: Model | undefined = undefined;

export async function fetchModelCached() {
  if (!model) {
    model = await fetchModel();
  }
  return model;
}
