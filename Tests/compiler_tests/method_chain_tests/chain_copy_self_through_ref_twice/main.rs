// r.sum() twice on same &ref — Copy struct, self:Self through auto-deref.
// Proves the ref is not consumed by the first call.
use Std.Marker.Copy;
struct Point { x: i32, y: i32 }
impl Copy for Point {}
impl Point { fn sum(self: Self) -> i32 { self.x + self.y } }
fn main() -> i32 {
    let p = make Point { x: 20, y: 22 };
    let r: &Point = &p;
    let a = r.sum();
    let b = r.sum();
    a + b - 42
}
