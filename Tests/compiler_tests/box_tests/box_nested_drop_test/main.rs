struct Inner {
    data: Box(i32)
}

struct Outer {
    inner: Inner,
    tag: i32
}

fn main() -> i32 {
    {
        let o = make Outer {
            inner: make Inner { data: Box.New(999) },
            tag: 7
        };
    }
    42
}
