use Std.Ops.Drop;
use Std.Mem.ManuallyDrop;

struct Counter['a] {
    r: &'a uniq i32
}

impl['a] Drop for Counter['a] {
    fn Drop(self: &uniq Self) {
        *self.r = *self.r + 10;
    }
}

fn main() -> i32 {
    let result = 0;
    {
        let c = make Counter { r: &uniq result };
        let _md = ManuallyDrop.New(c);
    };
    result
}
