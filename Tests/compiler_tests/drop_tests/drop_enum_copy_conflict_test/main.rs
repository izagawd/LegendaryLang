use Std.Ops.Drop;
enum Color {
    Red,
    Blue
}

impl Copy for Color {}

impl Drop for Color {
    fn Drop(self: &mut Self) {}
}

fn main() -> i32 {
    0
}
