// b.get_x().get_val() — Box<Outer> auto-derefs to Outer. get_x returns &Inner (Copy).
// Then get_val takes &Self on Inner through auto-deref of &Inner.
use Std.Marker.Copy;
struct Inner { val: i32 }
impl Copy for Inner {}
struct Outer { x: Inner }
impl Outer { fn get_x(self: &Self) -> Inner { self.x } }
impl Inner { fn get_val(self: &Self) -> i32 { self.val } }
fn main() -> i32 {
    let b = Box.New(make Outer { x: make Inner { val: 42 } });
    b.get_x().get_val()
}
