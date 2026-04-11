use Std.Ops.Drop;

struct Counter['a] {
    val: i32,
    target: &'a mut i32
}

impl['a] Counter['a] {
    fn inc(self: Self) -> Counter['a] {
        make Counter { val: self.val + 1, target: self.target }
    }
}

impl['a] Drop for Counter['a] {
    fn Drop(self: &mut Self) {
        *self.target = *self.target + 1;
    }
}

fn main() -> i32 {
    let drops = 0;
    let c = make Counter { val: 0, target: &mut drops }.inc().inc();
    drops
}
