use Std.Marker.MutReassign;

enum Maybe(T:! Sized) {
    Some(T),
    None
}
impl MutReassign for Maybe(i32) {}

fn main() -> i32 { 0 }
