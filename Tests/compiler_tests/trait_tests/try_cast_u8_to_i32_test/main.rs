use Std.Primitive.TryCastPrimitive;

fn main() -> i32 {
    let x: u8 = 200;
    let result = TryCastPrimitive(i32, x);
    match result {
        Option.Some(val) => val,
        Option.None => 0 - 1
    }
}
