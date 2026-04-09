trait Greet {
    fn hello(self: &Self) -> i32;
}
struct Foo { val: i32 }
impl Greet for Foo {
    fn hello(self: &Self) -> i32 { self.val }
}
fn main() -> i32 {
    let f = make Foo { val: 10 };
    f.hello()
}
