// make Tracker { counter: &mut c, val: 42 }.consume() takes self: Self.
// The temporary is moved into the method. At method exit, self is dropped,
// incrementing counter by 100. Method returns self.val (42).
// Result: 42 + 100 = 142.
use Std.Ops.Drop;

struct Tracker['a] {
    counter: &'a mut i32,
    val: i32
}

impl['a] Tracker['a] {
    fn consume(self: Self) -> i32 {
        self.val
    }
}

impl['a] Drop for Tracker['a] {
    fn Drop(self: &uniq Self) {
        *self.counter = *self.counter + 100;
    }
}

fn main() -> i32 {
    let c = 0;
    let v = make Tracker { counter: &mut c, val: 42 }.consume();
    v + c
}
