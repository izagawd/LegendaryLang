struct Foo { val: i32 }
impl Foo {
    fn set(self: &mut Self, v: i32) { self.val = v; }
}
fn main() -> i32 {
    let f = make Foo { val: 0 };
    let r = &f;
    r.set(42);
    f.val
}
