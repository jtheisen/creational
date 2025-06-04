import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export function intersperse<T>(separator: T, source: T[]) {
  const n = source.length;
  if (n <= 1) {
    return source;
  }
  const target = new Array<T>(n * 2 - 1);
  for (let i = 0; i < n; ++i) {
    target[i * 2] = source[i];
  }
  for (let i = 1; i < 2 * n - 1; i += 2) {
    target[i] = separator;
  }
  return target;
}

export function createBaseClass<T>(): { new (value: T): T } {
  function BaseClass(this: T, value: T) {
    Object.assign(this as any, value);
  }
  return BaseClass as any as { new (value: T): T };
}

export function deriveObject<B extends object, U extends object>(
  base: B,
  deriv: U & ThisType<B & U>
): B & U {
  Object.setPrototypeOf(deriv, base);
  return deriv as any as B & U;
}
