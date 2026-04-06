use Std.Marker.MutReassign;

enum Maybe(T:! type) {
    Some(T),
    None
}
impl MutReassign for Maybe(i32) {}

fn main() -> i32 { 0 }
