// self: Self — instance method (consuming), callable via x.consume()
struct Foo { val: i32 }
impl Foo {
    fn consume(self: Self) -> i32 { self.val }
}
fn main() -> i32 {
    let f = make Foo { val: 42 };
    f.consume()
}
