struct RefBox[T:! type] { inner: &T }
fn peek[T:! type](rb: RefBox(T)) -> &T { rb.inner }
fn main() -> i32 {
    let v: i32 = 77;
    let rb = make RefBox { inner: &v };
    *peek(rb)
}
