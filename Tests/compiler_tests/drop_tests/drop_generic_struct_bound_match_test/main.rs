use Std.Ops.Drop;
trait Marker {}
impl Marker for i32 {}

struct Wrapper['a](T:! Sized +Marker) {
    val: T,
    r: &'a mut i32
}

impl['a, T:! Sized +Marker] Drop for Wrapper('a, T) {
    fn Drop(self: &mut Self) {
        *self.r = *self.r + 1;
    }
}

fn main() -> i32 {
    let counter = 0;
    {
        let w = make Wrapper(i32) { val : 42, r : &mut counter };
    }
    counter
}
