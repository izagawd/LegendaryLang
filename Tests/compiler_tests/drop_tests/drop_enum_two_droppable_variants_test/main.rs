use Std.Ops.Drop;

struct CounterA['a] { r: &'a mut i32 }
struct CounterB['a] { r: &'a mut i32 }

impl['a] Drop for CounterA['a] {
    fn Drop(self: &uniq Self) { *self.r = *self.r + 10; }
}
impl['a] Drop for CounterB['a] {
    fn Drop(self: &uniq Self) { *self.r = *self.r + 100; }
}

enum Choice['a] {
    A(CounterA['a]),
    B(CounterB['a])
}

fn main() -> i32 {
    let counter = 0;
    {
        let c = Choice.A(make CounterA { r: &mut counter });
    };
    counter
}
