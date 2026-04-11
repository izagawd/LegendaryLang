// b.get_ref() returns &i32 (shared). b.set(99) takes &mut Self.
// & and &mut CAN coexist — &mut is shared-mutable, not exclusive.
// After set(99), *r reads the mutated value. Result: 99.
struct Foo { val: i32 }
impl Foo {
    fn get_ref(self: &Self) -> &i32 { &self.val }
    fn set(self: &mut Self, v: i32) { self.val = v; }
}
fn main() -> i32 {
    let b = Gc.New(make Foo { val: 42 });
    let r: &i32 = b.get_ref();
    b.set(99);
    *r
}
