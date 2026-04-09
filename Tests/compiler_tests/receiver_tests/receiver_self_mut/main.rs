// self: &mut Self — instance method, callable via x.mutate()
struct Foo { val: i32 }
impl Foo {
    fn mutate(self: &mut Self, v: i32) { self.val = v; }
    fn get(self: &Self) -> i32 { self.val }
}
fn main() -> i32 {
    let f = make Foo { val: 0 };
    f.mutate(42);
    f.get()
}
