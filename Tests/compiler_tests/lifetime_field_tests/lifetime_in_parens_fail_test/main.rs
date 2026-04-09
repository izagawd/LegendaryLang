struct Inner['a] {
    ptr: &'a i32
}

struct Outer['a, 'b] {
    x: &'a i32,
    inner: Inner('b)
}

fn main() -> i32 { 0 }
