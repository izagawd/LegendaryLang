trait A {
    fn doit(self: &Self) -> i32;
}
trait B {
    fn doit(self: &Self) -> i32;
}
struct Foo { val: i32 }
impl Copy for Foo {}
impl A for Foo {
    fn doit(self: &Self) -> i32 { 1 }
}
impl B for Foo {
    fn doit(self: &Self) -> i32 { 2 }
}
fn main() -> i32 {
    let f = make Foo { val: 0 };
    (Foo as A).doit(&f) + (Foo as B).doit(&f)
}
