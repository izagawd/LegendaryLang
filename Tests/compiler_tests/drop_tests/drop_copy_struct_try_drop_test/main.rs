use Std.Core.Marker.Drop;
struct Point {
    x: i32,
    y: i32
}

impl Copy for Point {}

impl Drop for Point {
    fn Drop(self: &uniq Self) {}
}

fn main() -> i32 {
    let p = make Point { x : 1, y : 2 };
    p.x + p.y
}
