use Std.Core.Marker.Drop;
trait Marker {}
impl Marker for i32 {}

struct Wrapper['a](T:! Marker) {
    val: T,
    r: &'a uniq i32
}

impl['a, T:! Marker] Drop for Wrapper('a, T) {
    fn Drop(self: &uniq Self) {
        *self.r = *self.r + 1;
    }
}

fn main() -> i32 {
    let counter = 0;
    {
        let w = make Wrapper(i32) { val : 42, r : &uniq counter };
    }
    counter
}
