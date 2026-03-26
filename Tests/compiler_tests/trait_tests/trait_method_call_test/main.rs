trait Greet {
    fn value(self: &Self) -> i32;
}
struct Foo {
    val: i32
}
impl Greet for Foo {
    fn value(self: &Foo) -> i32 {
        self.val
    }
}
fn main() -> i32 {
    let f = Foo { val = 42 };
    f.value()
}
