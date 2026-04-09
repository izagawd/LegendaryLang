// self: &Self — instance method, callable via x.get()
struct Foo { val: i32 }
impl Foo {
    fn get(self: &Self) -> i32 { self.val }
}
fn main() -> i32 {
    let f = make Foo { val: 42 };
    f.get()
}
