use Std.Ops.Drop;
struct Foo {
    x: i32
}

impl Drop for Foo {
    fn Drop(self: &mut Self) {}
}
impl Copy for Foo {}

fn main() -> i32 {
    0
}
