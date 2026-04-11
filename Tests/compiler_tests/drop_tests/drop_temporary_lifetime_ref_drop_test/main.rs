// make Tracker { counter: &mut c, val: 42 }.get_ref() returns &i32.
// The temporary Tracker is spilled to the current scope. After *r is read (42),
// the scope exits, Tracker drops, incrementing c to 100. Result: 42 + 100 = 142.
use Std.Ops.Drop;

struct Tracker['a] {
    counter: &'a mut i32,
    val: i32
}

impl['a] Tracker['a] {
    fn get_ref(self: &Self) -> &i32 {
        &self.val
    }
}

impl['a] Drop for Tracker['a] {
    fn Drop(self: &mut Self) {
        *self.counter = *self.counter + 100;
    }
}

fn main() -> i32 {
    let c = 0;
    let result = {
        let r: &i32 = make Tracker { counter: &mut c, val: 42 }.get_ref();
        *r
    };
    result + c
}
