use Std.Ops.Drop;

struct Idk['a] { dd: &'a mut i32 }

impl['a] Drop for Idk['a] {
    fn Drop(self: &uniq Self) {
        *self.dd = *self.dd + 1;
    }
}

fn main() -> i32 {
    let dd = 5;
    {
        let bro = make Idk { dd: &mut dd };
        let bro = make Idk { dd: &mut dd };
    }
    dd
}
