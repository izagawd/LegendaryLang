use Std.Ops.Drop;
use Std.Alloc.Box;

struct Droppable['a] {
    reference: &'a mut i32
}

impl['a] Drop for Droppable['a] {
    fn Drop(self: &mut Self) {
        *self.reference = *self.reference + 1;
    }
}

fn DropNow[T:! type](input: T) {}

fn main() -> i32 {
    let counter = 0;
    let b = Box.New(make Droppable { reference: &mut counter });
    DropNow(b);
    counter
}
