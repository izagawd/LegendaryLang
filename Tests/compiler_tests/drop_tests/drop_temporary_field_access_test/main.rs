use Std.Ops.Drop;

struct Tracker['a] {
    counter: &'a mut i32
}

impl['a] Drop for Tracker['a] {
    fn Drop(self: &uniq Self) {
        *self.counter = *self.counter + 100;
    }
}

struct Wrapper['a] {
    val: i32,
    tracker: Tracker['a]
}

fn main() -> i32 {
    let c = 0;
    let v = {
        let w = make Wrapper { val: 7, tracker: make Tracker { counter: &mut c } };
        w.val
    };
    v + c
}
