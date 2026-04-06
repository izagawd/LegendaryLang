use Std.Core.Marker.MutReassign;

struct Point { x: i32, y: i32 }
impl Copy for Point {}
impl MutReassign for Point {}

fn main() -> i32 {
    let p = make Point { x: 0, y: 0 };
    let r = &mut p;
    *r = make Point { x: 10, y: 20 };
    p.x + p.y
}
