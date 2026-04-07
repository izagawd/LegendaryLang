use Std.Primitive.TryCastPrimitive;

fn main() -> i32 {
    let result = TryCastPrimitive(usize, 100);
    match result {
        Option.Some(val) => 1,
        Option.None => 0
    }
}
