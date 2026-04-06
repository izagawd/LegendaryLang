use Std.Core.Marker.Drop;
enum Color {
    Red,
    Blue
}

impl Copy for Color {}

impl Drop for Color {
    fn Drop(self: &uniq Self) {}
}

fn main() -> i32 {
    0
}
