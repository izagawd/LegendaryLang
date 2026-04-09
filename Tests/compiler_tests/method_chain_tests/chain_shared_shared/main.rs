// x.get_a().get_b() — two &Self methods chained. Both return i32.
// get_a returns Inner by value (Copy), get_b reads from it.
use Std.Marker.Copy;
struct Outer { a: Inner }
struct Inner { val: i32 }
impl Copy for Inner {}
impl Outer { fn get_a(self: &Self) -> Inner { self.a } }
impl Inner { fn get_b(self: &Self) -> i32 { self.val } }
fn main() -> i32 {
    let o = make Outer { a: make Inner { val: 42 } };
    o.get_a().get_b()
}
