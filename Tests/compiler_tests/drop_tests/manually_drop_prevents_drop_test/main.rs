use Std.Ops.Drop;
use Std.Mem.ManuallyDrop;

struct Counter {
    r: &uniq i32
}

impl Drop for Counter {
    fn Drop(self: &uniq Self) {
        *self.r = *self.r + 10;
    }
}

fn main() -> i32 {
    let result = 0;
    {
        let c = make Counter { r: &uniq result };
        let _md = make ManuallyDrop(Counter) { val: c };
    };
    result
}
