struct Inner {
    data: Gc(i32)
}

struct Outer {
    inner: Inner,
    tag: i32
}

fn main() -> i32 {
    {
        let o = make Outer {
            inner: make Inner { data: Gc.New(999) },
            tag: 7
        };
    }
    42
}
