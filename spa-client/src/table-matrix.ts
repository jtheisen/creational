// export class TableMatrix<T> {
//   private rows: T[][];

//   constructor(rows: T[][]) {
//     this.rows = rows;
//   }

//   transposeMatrix() {
//     return new TableMatrix(
//       this.rows[0].map((_, col) => this.rows.map((row) => row[col]))
//     );
//   }

//   renderHtmlCell(c, m) {
//     if (m.drop) return "";
//     const { style, className } = m;
//     const colspan = m.colspan ?? 1;
//     return m.header
//       ? html`<th colspan=${colspan} style=${style} class=${className}>${c}</th>`
//       : html`<td colspan=${colspan} style=${style} class=${className}>
//           ${c}
//         </td>`;
//   }

//   renderHtmlTableContent() {
//     return html`
//       ${this.rows.map(
//         (r) => html`<tr>
//           ${r.map(([c, m]) => this.renderHtmlCell(c, m))}
//         </tr>`
//       )}
//     `;
//   }

//   renderHtmlTable() {
//     return html`<table>
//       <tbody>
//         ${this.renderHtmlTableContent()}
//       </tbody>
//     </table> `;
//   }

//   map<U>(func: (s: T) => U) {
//     return new TableMatrix(this.rows.map((r) => r.map(func)));
//   }

//   // Doesn't work as Observable doesn't allow us to construct markdown dynamically
//   renderMarkdownTableContent() {
//     return html`
//       ${this.rows.map((r) => `\n|${r.map((c) => `${c}`).join("|")}|`)}
//     `;
//   }

//   extendBelow(tableMatrix: TableMatrix<T>) {
//     return new TableMatrix([...this.rows, ...tableMatrix.rows]);
//   }
// }

//   function createTableMatrixFromItemsArray<T>(list: T[]) {
//     return new TableMatrix([list]);
//   }

//   function createFromArrayOfObjects<O>(list: O[]) {
//     if (list.length === 0) {
//       return new TableMatrix([]);
//     }
//     const keys = Object.keys(list[0]);
//     const rows = list.map((r) => keys.map((k) => [r[k], {}]));
//     rows.unshift(keys.map((k) => [k, { header: true }]));
//     return new TableMatrix(rows);
//   }
