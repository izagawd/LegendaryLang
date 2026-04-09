// dd: &Self — first param is NOT named self. NOT an instance method.
// Calling via x.bar() should fail: no method 'bar' found.
struct Foo { val: i32 }
impl Foo {
    fn bar(dd: &Self) -> i32 { dd.val }
}
fn main() -> i32 {
    let f = make Foo { val: 42 };
    f.bar()
}
