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

struct Wrapper['a] {
    inner: Droppable['a]
}

fn DropNow[T:! type](input: T) {}

fn main() -> i32 {
    let counter = 0;
    let b = Box.New(make Wrapper { inner: make Droppable { reference: &mut counter } });
    DropNow(b);
    counter
}
