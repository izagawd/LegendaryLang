use Std.Marker.MutReassign;

struct Inner { val: i32 }
struct Outer { inner: Inner }
impl MutReassign for Outer {}

fn main() -> i32 { 0 }
