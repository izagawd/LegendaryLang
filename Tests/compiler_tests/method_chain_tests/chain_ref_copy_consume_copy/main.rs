// r.extract().double() — r is &Pair (Copy). extract takes &Self returns Num (Copy).
// double takes self:Self on Num. Chain: auto-deref &Pair → Pair(&Self) → Num → Num(Self).
use Std.Marker.Copy;
struct Pair { a: i32, b: i32 }
struct Num { v: i32 }
impl Copy for Pair {}
impl Copy for Num {}
impl Pair { fn extract(self: &Self) -> Num { make Num { v: self.a + self.b } } }
impl Num { fn double(self: Self) -> i32 { self.v + self.v } }
fn main() -> i32 {
    let p = make Pair { a: 10, b: 11 };
    let r: &Pair = &p;
    r.extract().double()
}
