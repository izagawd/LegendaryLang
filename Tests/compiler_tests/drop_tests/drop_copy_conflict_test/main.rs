use Std.Ops.Drop;
struct Foo {
    x: i32
}

impl Copy for Foo {}
impl Drop for Foo {
    fn Drop(self: &uniq Self) {}
}

fn main() -> i32 {
    0
}
