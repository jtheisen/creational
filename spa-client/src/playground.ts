function createType<P>(proto: P) {}

createType({
  foo: 2,
  bar: 0,

  getFoo() {
    return this.foo;
  },
});
