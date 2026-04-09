use Std.Alloc.Box;

fn get_static() -> &'static i32 {
    let b = Box.New(42);
    &*Box.Leak(b)
}

fn main() -> i32 { *get_static() }
