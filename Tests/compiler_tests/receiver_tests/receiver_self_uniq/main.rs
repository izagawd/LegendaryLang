// self: &uniq Self — instance method, callable via x.set()
struct Foo { val: i32 }
impl Foo {
    fn set(self: &uniq Self, v: i32) { self.val = v; }
    fn get(self: &Self) -> i32 { self.val }
}
fn main() -> i32 {
    let f = make Foo { val: 0 };
    f.set(42);
    f.get()
}
