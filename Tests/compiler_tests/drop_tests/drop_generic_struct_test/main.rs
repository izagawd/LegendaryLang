use Std.Core.Marker.Drop;
struct Wrapper['a](T:! type) {
    val: T,
    r: &'a uniq i32
}

impl['a, T:! type] Drop for Wrapper('a, T) {
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
