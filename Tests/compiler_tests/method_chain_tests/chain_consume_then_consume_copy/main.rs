// x.double().add_ten() — both self:Self on Copy struct. Each consumes and returns new.
use Std.Marker.Copy;
struct Num { v: i32 }
impl Copy for Num {}
impl Num {
    fn double(self: Self) -> Num { make Num { v: self.v + self.v } }
    fn add_ten(self: Self) -> i32 { self.v + 10 }
}
fn main() -> i32 {
    let n = make Num { v: 16 };
    n.double().add_ten()
}
