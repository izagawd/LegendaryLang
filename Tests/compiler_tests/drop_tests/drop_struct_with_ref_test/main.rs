use Std.Ops.Drop;
struct Guard['a] {
    target: &'a mut i32
}

impl['a] Drop for Guard['a] {
    fn Drop(self: &mut Self) {
        *self.target = *self.target + 100;
    }
}

fn main() -> i32 {
    let val = 0;
    {
        let g = make Guard { target : &mut val };
    }
    val
}
