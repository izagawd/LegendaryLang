use Std.Ops.Drop;

struct Incrementer['a] {
    target: &'a mut i32
}

impl['a] Drop for Incrementer['a] {
    fn Drop(self: &mut Self) {
        *self.target = *self.target + 1;
    }
}

fn main() -> i32 {
    let a = 5;
    let dd = make Incrementer { target: &mut a };
    a
}
