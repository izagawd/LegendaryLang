use Std.Ops.Drop;

struct Counter['a] {
    val: &'a mut i32
}

impl['a] Drop for Counter['a] {
    fn Drop(self: &uniq Self) {
        *self.val = *self.val + 10;
    }
}

fn make_counter['a](v: &'a mut i32) -> Counter['a] {
    make Counter { val: v }
}

fn main() -> i32 {
    let x = 0;
    make_counter(&mut x);
    x
}
