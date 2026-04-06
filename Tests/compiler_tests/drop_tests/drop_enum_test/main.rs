use Std.Ops.Drop;
enum Action {
    Increment(i32),
    Nothing
}

struct Watcher['a] {
    r: &'a uniq i32
}

impl['a] Drop for Watcher['a] {
    fn Drop(self: &uniq Self) {
        *self.r = *self.r + 1;
    }
}

fn main() -> i32 {
    let counter = 0;
    {
        let w = make Watcher { r : &uniq counter };
    }
    counter
}
