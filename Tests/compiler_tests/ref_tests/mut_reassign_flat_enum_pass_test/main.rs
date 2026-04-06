use Std.Core.Marker.MutReassign;

enum Color { Red, Green, Blue }
impl Copy for Color {}
impl MutReassign for Color {}

fn main() -> i32 {
    let c = Color.Red;
    let r = &mut c;
    *r = Color.Blue;
    match c {
        Color.Red => 1,
        Color.Green => 2,
        Color.Blue => 3
    }
}
