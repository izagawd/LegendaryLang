// dd: &Self — not named self, so it's a static method.
// Callable via Foo.bar(&x) with explicit arg.
struct Foo { val: i32 }
impl Foo {
    fn bar(dd: &Foo) -> i32 { dd.val }
}
fn main() -> i32 {
    let f = make Foo { val: 42 };
    Foo.bar(&f)
}
