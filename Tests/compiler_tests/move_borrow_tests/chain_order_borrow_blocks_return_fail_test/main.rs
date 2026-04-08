use Std.Ops.Drop;

struct Tracker['a] {
    id: i32,
    counter: &'a mut i32
}

impl['a] Tracker['a] {
    fn chain(self: Self, next_id: i32) -> Tracker['a] {
        make Tracker { id: next_id, counter: self.counter }
    }
}

impl['a] Drop for Tracker['a] {
    fn Drop(self: &uniq Self) {
        *self.counter = *self.counter * 10 + self.id;
    }
}

fn main() -> i32 {
    let order = 0;
    let result = make Tracker { id: 1, counter: &mut order }.chain(2).chain(3);
    order
}
