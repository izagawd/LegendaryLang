use Std.Ops.Drop;

struct CounterA { r: &mut i32 }
struct CounterB { r: &mut i32 }

impl Drop for CounterA {
    fn Drop(self: &uniq Self) { *self.r = *self.r + 10; }
}
impl Drop for CounterB {
    fn Drop(self: &uniq Self) { *self.r = *self.r + 100; }
}

enum Choice {
    A(CounterA),
    B(CounterB)
}

fn main() -> i32 {
    let counter = 0;
    {
        let c = Choice.A(make CounterA { r: &mut counter });
    };
    counter
}
